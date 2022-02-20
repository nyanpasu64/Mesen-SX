#include "stdafx.h"
#include <assert.h>
#include "SimpleLock.h"

SimpleLock::SimpleLock()
{
}

SimpleLock::~SimpleLock()
{
}

LockHandler SimpleLock::AcquireSafe()
{
	return LockHandler(this);
}

void SimpleLock::Acquire()
{
	_mutex.lock();
}

void SimpleLock::WaitForRelease()
{
	//Wait until we are able to grab a lock, and then release it again
	Acquire();
	Release();
}

void SimpleLock::Release()
{
	_mutex.unlock();
}


LockHandler::LockHandler(SimpleLock *lock)
{
	_lock = lock;
	_lock->Acquire();
}

LockHandler::~LockHandler()
{
	_lock->Release();
}